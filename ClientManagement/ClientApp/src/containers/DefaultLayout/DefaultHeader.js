import React, { Component } from 'react';
import { Nav, NavItem, NavLink } from 'reactstrap';
import PropTypes from 'prop-types';

import { AppAsideToggler, AppNavbarBrand, AppSidebarToggler } from '@coreui/react';
import DefaultHeaderDropdown from './DefaultHeaderDropdown';
import logoLrg from '../../assets/img/brand/LogoLarge.jpg';
import logoSml from '../../assets/img/brand/LogoSmall.jpg';

const propTypes = {
  children: PropTypes.node
};

const defaultProps = {};

class DefaultHeader extends Component {
  render() {

    // eslint-disable-next-line
    const { children, ...attributes } = this.props;

    return (
      <React.Fragment>
        <AppSidebarToggler className="d-lg-none" display="md" mobile />
        <AppNavbarBrand
            full={{ src: logoLrg, width: 89, height: 25, alt: 'Fiatica' }}
            minimized={{ src: logoSml, width: 30, height: 30, alt: 'Fiatica' }}
        />
        <AppSidebarToggler className="d-md-down-none" display="lg" />
        <Nav className="ml-auto" navbar>
          <DefaultHeaderDropdown accnt />
          <DefaultHeaderDropdown plchr />
        </Nav>
      </React.Fragment>
    );
  }
}

DefaultHeader.propTypes = propTypes;
DefaultHeader.defaultProps = defaultProps;

export default DefaultHeader;
